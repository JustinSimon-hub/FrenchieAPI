using System;
using FrenchieAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FrenchieAPI.Data
{
    public class FrencieAPIDbContext : DbContext
    {
        public FrencieAPIDbContext(DbContextOptions options) : base(options)
        {
            //this is called in every instance of the database that allows congruency between the set

        }

        //this represents the db that will be commnicated in the controller class
        //controllers are classes that directly communicate with the models class and manipulate data(throguh the use of action methods that affect the data)

        public DbSet<Frenchie> Frenchies { get; set; }


    }
}

