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

        public TaskGroupViewModel_Populated( TaskGroup tg, Func<int, Task> DeleteGroupAction )
        {
            Task_Group = tg;
            _deleteGroup = DeleteGroupAction;
        }

        private Action<TaskGroupViewModel_Populated> _selectionChanged;
        private Func<int, Task> _retaskAction;
        private Func<int, Task> _deleteGroup;

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

        private AsyncCommand<int> deleteCommand;
        public AsyncCommand<int> DeleteCommand => deleteCommand ??= new AsyncCommand<int>( Delete );

        private async Task ReTask()
        {
            await _retaskAction.Invoke( Id );
        }

        private async Task Delete( int id )
        {
            await DeleteYourself( id, false );
            await _deleteGroup.Invoke( id );
        }
    }
}
