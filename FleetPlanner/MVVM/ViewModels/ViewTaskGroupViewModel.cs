using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class ViewTaskGroupViewModel : TaskGroupViewModel
    {

        #region Methods
        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.TaskGroup:
                    Task_Group = kvp.Value as TaskGroup;
                    await Populate();
                    break;

                default:
                    break;
            }

            await base.EvaluateQueryParams( kvp );
        }

        private async Task Populate()
        {
            Console.WriteLine( $"----------------- Populating {Task_Group.Name} -----------------" );
        }
        #endregion Query Handling
        #endregion Methods
    }
}
