using FleetPlanner.MVVM.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class TaskGroupViewModel_Populated : TaskGroupViewModel
    {
        public TaskGroupViewModel_Populated( TaskGroup tg )
        {
            Task_Group = tg;
        }

        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty( ref isChecked, value );
        }
    }
}
