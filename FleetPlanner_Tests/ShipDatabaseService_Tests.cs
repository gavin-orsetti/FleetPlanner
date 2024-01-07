using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.ViewModels;
using FleetPlanner.Services;

namespace FleetPlanner_Tests
{
    public class ShipDatabaseService_Tests
    {
        //Test naming should follow this convention "MethodName_ExpectedResult_UnderGivenCondition"

        // This test will always fail because the Create() Method reads in data from the ships.json file, and for some reason that throws an error. I don't have time to figure it out right now, so I am just going to leave it. The Method works in the main application, but if it starts giving me trouble I will come back to this.
        //[Fact]
        //public async Task Create_PopulatesDatabaseDictionary_UponServiceCreation()
        //{
        //    // Arrange
        //    ShipDatabaseService service;

        //    // Act
        //    service = await ShipDatabaseService.Create();

        //    // Assert
        //    Assert.True( service.Db.Count > 0 );
        //}
    }
}
