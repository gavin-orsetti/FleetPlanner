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
        public TaskGroupViewModel_Populated( GlobalViewModel global, TaskGroup tg, Action<TaskGroupViewModel_Populated> selectionChangedAction, Func<int, Task> RetaskAction, Func<int, Task> deleteGroupAction ) : base( global )
        {
            Task_Group = tg;
            _selectionChanged = selectionChangedAction != null ? selectionChangedAction : _selectionChanged;
            _retaskAction = RetaskAction != null ? RetaskAction : _retaskAction;
            _deleteGroup = deleteGroupAction != null ? deleteGroupAction : _deleteGroup;
        }

        //TODO: Revisit this decision and consider allowing these to be null in order to fail out rather than just sending a call into a difficult to track down void of nothingness.
        // All three of the below (Action/2 Func) need to have a default value of an empty delegate, to avoid a null reference exception.
        private Action<TaskGroupViewModel_Populated> _selectionChanged = _ => { };
        private Func<int, Task> _retaskAction = async _ => { };
        private Func<int, Task> _deleteGroup = async _ => { };

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
