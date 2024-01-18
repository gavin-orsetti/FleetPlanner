using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TaskGroupViewModel : ViewModelBase
    {
        //private TaskGroup taskGroup;
        //public TaskGroup TaskGroup { get { return taskGroup; } }

        private string name;
        public string Name
        {
            get => name ??= string.Empty;
            set => SetProperty( ref name, value );
        }


        #region Methods
        #region Query Handling
        private protected override async Task EvaluateQueryParams( KeyValuePair<string, object> kvp )
        {
            switch( kvp.Key )
            {
                case Routes.TaskGroupQueryParams.FleetId:
                    //await Task.Delay( 1 );
                    break;
                default:
                    break;
            }
        }
        #endregion Query Handling
        #endregion Methods

    }
}
