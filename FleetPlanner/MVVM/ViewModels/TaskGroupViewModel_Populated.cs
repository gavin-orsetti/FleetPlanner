using FleetPlanner.MVVM.Models;

using MvvmHelpers.Commands;

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

        public TaskGroupViewModel_Populated( TaskGroup tg, Action<TaskGroupViewModel_Populated> selectionChangedAction, Func<int, Task> RetaskAction )
        {
            Task_Group = tg;
            _selectionChanged = selectionChangedAction;
            _retaskAction = RetaskAction;
        }

        private Action<TaskGroupViewModel_Populated> _selectionChanged;
        private Func<int, Task> _retaskAction;

        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                SetProperty( ref isChecked, value );

                if( value )
                    _selectionChanged.Invoke( this );

            }
        }

        private AsyncCommand retaskCommand;
        public AsyncCommand RetaskCommand => retaskCommand ??= new AsyncCommand( ReTask );

        private async Task ReTask()
        {
            await _retaskAction.Invoke( Id );
        }
    }
}
