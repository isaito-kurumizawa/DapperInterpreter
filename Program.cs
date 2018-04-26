using System;
using System.Collections.Generic;
using mygkrnk.Models;
using Dapper;
namespace originalDapeperLib
{
    class Program
    {
        static void Main(string[] args)
        {
            // new DapperInterpreter().FindByTypes<Musics>(new Musics(){ id = 136, Name = "Paris" });
            // new DapperInterpreter().FindByList<Musics>(new List<int> {15, 40, 25, 33});
            new DapperInterpreter("").Update<TEST>(new TEST(){Semester = "unforgetable", CreateTime = DateTime.Now.AddDays(+60).Date});
            Console.WriteLine("Hello World!");
        }
    }
}
