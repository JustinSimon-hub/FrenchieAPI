using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrenchieAPI.Data;
using FrenchieAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FrenchieAPI.Controllers
{
    //Establish route taken by the action methods
    [ApiController]
    [Route("api/[controller]")]
    //the name in the route has to match the name inside the controllers  name
    //minus the word controller and exludes the capitilzation

    public class FrenchieController : Controller
    {
        //this property is responsible for talking to the database.

        private readonly FrencieAPIDbContext dbContext;

        public FrenchieController(FrencieAPIDbContext dbContext)
        {
            //creates seperation and security of the db
            //and 
            this.dbContext = dbContext;

        }


        [HttpGet]
        [Route("{id:guid}")]
        //retrieve single resource
        public async Task<IActionResult> GetFrenchie([FromRoute] Guid id)
        {
            var frenchie = await dbContext.Frenchies.FindAsync(id);

            if(frenchie == null)
            {
                return NotFound("The resource was not found");
            }

            return Ok(frenchie);


        }


        [HttpGet]
        public async Task<IActionResult> GetFrenchies()
        {
            return Ok(await dbContext.Frenchies.ToListAsync());
        }


        //Creation of http resources
        [HttpPost]
            //get all frenchie resources
       public async Task<IActionResult> CreateFrenchie(AddFrenchieRequest addFrenchieRequest)
        {
            var frenchie = new Frenchie()
            {
                Id = Guid.NewGuid(),
                Name = addFrenchieRequest.Name,
                Color = addFrenchieRequest.Color,
                Age = addFrenchieRequest.Age
            };
            await dbContext.Frenchies.AddAsync(frenchie);
            await dbContext.SaveChangesAsync();
            return Ok(frenchie);
        }


        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> UpdateFrenchie([FromRoute] Guid id, UpdateFrenchieRequest updateFrenchie)
        {
            var frenchie = await dbContext.Frenchies.FindAsync(id);
            if (frenchie != null)
            {
                frenchie.Age = updateFrenchie.Age;
                frenchie.Color = updateFrenchie.Color;
                frenchie.Name = updateFrenchie.Name;
                await dbContext.SaveChangesAsync();
                return Ok(frenchie);

            }
            else return NotFound("The frenchie could not be located");
           
        }


        [HttpDelete]
        [Route("{id:guid}")]
        public async Task<IActionResult> DeleteFrenchie([FromRoute] Guid id)
        {
            var frenchie = await dbContext.Frenchies.FindAsync(id);
            if(id != null)
            {
                dbContext.Remove(frenchie);
                await dbContext.SaveChangesAsync();
                return Ok("The frenchie object was successfully deleted");
            }
            return NotFound("Can't delete the frenchie/doesnt exist");
        }
    }
}

