using System;
using System.Collections.Generic;
using mygkrnk.Models;
using DapperInterpreter;
namespace originalDapeperLib
{
    class Program
    {
        static void Main(string[] args)
        {
            // new BaseRepository().FindByTypes<Musics>(new Musics(){ id = 136, Name = "Paris" });
            // new BaseRepository().FindByList<Musics>(new List<int> {15, 40, 25, 33});
            new BaseRepository("").Update<TEST>(new TEST(){Semester = "unforgetable", CreateTime = DateTime.Now.AddDays(+60).Date});
            Console.WriteLine("Hello World!");
        }
    }
}
