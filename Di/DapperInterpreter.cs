namespace Dapper
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Data;
    using System.Linq;
    using System.Data.SqlClient;
    using Dapper;

    public partial class DapperInterpreter
    {
        private string _connectionString;
        private string[] _dateTimeNames;
        public DapperInterpreter(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DapperInterpreter(string connectionString, params string[] dateTimeNames)
        {
            _connectionString = connectionString;
            _dateTimeNames = dateTimeNames;
        }

        protected SqlConnection GetOpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        protected IEnumerable<T> Query<T>(string sql, object param)
        {
            using (var connection = GetOpenConnection())
            {
                var results = connection.Query<T>(sql, param).ToList();
                return results;
            }
        }

        protected int Execute(string sql, object param, CommandType cmdType)
        {
            using (var connection = GetOpenConnection())
            {
                var results = connection.Execute(sql, param, commandType: cmdType);
                return results;
            }

        }

        public IEnumerable<T> FindByTypes<T>(T model = default(T))
        {
            var type = typeof(T);
            var columnString = GetColumnString(type.GetProperties());
            var whereString = GetConditionsString<T>(model);
            var results = Query<T>(string.Format("SELECT {0} FROM {1} {2}", columnString, type.Name, whereString), model);
            return results;
        }

        public IEnumerable<T> FindByList<T>(List<int> lists)
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            var columnString = GetColumnString(properties);
            var keyProperty = GetKeyProperty(properties);
            var param = new { Lists = lists };
            var results = Query<T>(string.Format("SELECT {0} FROM {1} WHERE {2} IN @Lists", columnString, type.Name, keyProperty.Name), param);
            return results;
        }
        public bool Update<T>(T model)
        {
            var type = typeof(T);
            var properties = type.GetProperties();
            var keyProperty = GetKeyProperty(properties);
            var resultCount = 0;

            if (CheckIsPropertyDefaultValue(keyProperty, model))
            {
                // Insert
                var columnString = GetColumnString(model.GetType().GetProperties()).Replace(keyProperty.Name + ",", "");
                var values = GetInsertValuesString(model);
                resultCount = Execute(string.Format("INSERT INTO {0} ({1}) VALUES ({2})", type.Name, columnString, values), model, CommandType.Text);
            }
            else
            {
                // Update
                var param = new { Key = keyProperty.GetValue(model) };
                var updateResults = Query<T>(string.Format("SELECT * FROM {0} WHERE {1} = @Key", type.Name, keyProperty.Name), param).ToList();

                var updateData = updateResults.First();
                foreach (var mp in model.GetType().GetProperties())
                {
                    var updateValue = mp.GetValue(model);
                    var updateDataProperty = updateData.GetType().GetProperties().Where(u => u.Name == mp.Name && u.GetMethod.ReturnType == mp.GetMethod.ReturnType).SingleOrDefault();
                    updateValue = (CheckIsPropertyDefaultValue(mp, model)) ? updateDataProperty.GetValue(updateData) : updateValue;
                    mp.SetValue(model, updateValue);
                }
                var setData = GetUpdateValuesString<T>(model);
                resultCount = Execute(string.Format("UPDATE {0} SET {1} WHERE {2} = @{2}", type.Name, setData, keyProperty.Name), model, CommandType.Text);
            }

            return (resultCount > 0);
        }

        private string GetColumnString(PropertyInfo[] propertyInfo)
        {
            var columnString = string.Empty;
            var properties = propertyInfo.Where(p => CheckIsPropertyColumType(p)).ToList();
            properties.Select(p => columnString += string.Format("{0},", p.Name)).ToList();
            return columnString.Substring(0, columnString.Length - 1);
        }
        private string GetConditionsString<T>(T model)
        {
            if (model == null)
                return "";
            var whereString = "Where ";
            var properties = model.GetType().GetProperties().Where(p => CheckIsPropertyColumType(p)).ToList();
            properties.Select(p => whereString += CheckIsPropertyDefaultValue(p, model) ? "" : string.Format("{0}=@{0} AND ", p.Name)).ToList();
            return whereString.Substring(0, whereString.Length - 5); ;
        }

        private string GetInsertValuesString<T>(T model)
        {
            var result = string.Empty;
            var properties = model.GetType().GetProperties().Where(p => CheckIsPropertyColumType(p)).ToArray();
            var keyProperty = GetKeyProperty(properties);
            foreach (var property in properties)
            {
                if (property == keyProperty)
                    continue;
                if (this._connectionString.Contains(property.Name))
                    property.SetValue(model, DateTime.Now);
                result += string.Format("@{0},", property.Name);
            }
            return result.Substring(0, result.Length - 1);
        }

        private string GetUpdateValuesString<T>(T model)
        {
            var result = string.Empty;
            var properties = model.GetType().GetProperties().Where(p => CheckIsPropertyColumType(p)).ToArray();
            var keyProperty = GetKeyProperty(properties);
            properties.Select(p => result += (p != keyProperty) ? string.Format("{0} = @{0},", p.Name) : "").ToList();
            return result.Substring(0, result.Length - 1);
        }

        private PropertyInfo GetKeyProperty(PropertyInfo[] properties)
        {
            return properties.Where(p => p.CustomAttributes.Where(c => c.AttributeType.ToString().Contains("KeyAttribute")).Any()).SingleOrDefault();
        }

        private bool CheckIsPropertyColumType(PropertyInfo property)
        {
            var returnType = property.GetMethod.ReturnType;
            if (returnType == typeof(byte) || returnType == typeof(sbyte) || returnType == typeof(short) || returnType == typeof(ushort) ||
                returnType == typeof(int) || returnType == typeof(uint) || returnType == typeof(long) || returnType == typeof(ulong) ||
                returnType == typeof(float) || returnType == typeof(double) || returnType == typeof(decimal) || returnType == typeof(bool) ||
                returnType == typeof(string) || returnType == typeof(DateTime) || returnType == typeof(byte[]) ||
                returnType == typeof(Nullable<byte>) || returnType == typeof(Nullable<sbyte>) || returnType == typeof(Nullable<short>) || returnType == typeof(Nullable<ushort>) ||
                returnType == typeof(Nullable<int>) || returnType == typeof(Nullable<uint>) || returnType == typeof(Nullable<long>) || returnType == typeof(Nullable<ulong>) ||
                returnType == typeof(Nullable<float>) || returnType == typeof(Nullable<double>) || returnType == typeof(Nullable<decimal>) || returnType == typeof(Nullable<bool>) ||
                returnType == typeof(Nullable<DateTime>))
            {
                return true;
            }
            return false;
        }
        private bool CheckIsPropertyDefaultValue<T>(PropertyInfo property, T model)
        {
            var returnType = property.GetMethod.ReturnType;
            var value = property.GetValue(model);
            if ((returnType == typeof(byte) && (byte)value == default(byte)) || (returnType == typeof(sbyte) && (sbyte)value == default(sbyte)) ||
                (returnType == typeof(short) && (short)value == default(short)) || (returnType == typeof(ushort) && (ushort)value == default(ushort)) ||
                (returnType == typeof(int) && (int)value == default(int)) || (returnType == typeof(uint) && (uint)value == default(uint)) ||
                (returnType == typeof(long) && (long)value == default(long)) || (returnType == typeof(ulong) && (ulong)value == default(ulong)) ||
                (returnType == typeof(float) && (float)value == default(float)) || (returnType == typeof(double) && (double)value == default(double)) ||
                (returnType == typeof(decimal) && (decimal)value == default(decimal)) || (returnType == typeof(bool) && (bool)value == default(bool)) ||
                (returnType == typeof(string) && (string)value == default(string)) || (returnType == typeof(DateTime) && (DateTime)value == default(DateTime)) ||
                (returnType == typeof(byte[]) && (byte[])value == default(byte[]) || (returnType == typeof(Nullable<byte>) && (Nullable<byte>)value == default(Nullable<byte>)) ||
                (returnType == typeof(Nullable<sbyte>) && (Nullable<sbyte>)value == default(Nullable<sbyte>)) || (returnType == typeof(Nullable<short>) && (Nullable<short>)value == default(Nullable<short>)) ||
                (returnType == typeof(Nullable<ushort>) && (Nullable<ushort>)value == default(Nullable<ushort>)) || (returnType == typeof(Nullable<int>) && (Nullable<int>)value == default(Nullable<int>)) ||
                (returnType == typeof(Nullable<uint>) && (Nullable<uint>)value == default(Nullable<uint>)) || (returnType == typeof(Nullable<long>) && (Nullable<long>)value == default(Nullable<long>)) ||
                (returnType == typeof(Nullable<ulong>) && (Nullable<ulong>)value == default(Nullable<ulong>)) || (returnType == typeof(Nullable<float>) && (Nullable<float>)value == default(Nullable<float>)) ||
                (returnType == typeof(Nullable<double>) && (Nullable<double>)value == default(Nullable<double>)) || (returnType == typeof(Nullable<decimal>) && (Nullable<decimal>)value == default(Nullable<decimal>)) ||
                (returnType == typeof(Nullable<bool>) && (Nullable<bool>)value == default(Nullable<bool>)) || (returnType == typeof(Nullable<DateTime>) && (Nullable<DateTime>)value == default(Nullable<DateTime>))))
            {
                return true;
            }
            return false;
        }
    }
}