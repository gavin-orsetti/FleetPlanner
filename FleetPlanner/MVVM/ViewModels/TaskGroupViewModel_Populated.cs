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

        public TaskGroupViewModel_Populated( TaskGroup tg, Action<int> selectionChangedAction )
        {
            Task_Group = tg;
            _selectionChanged = selectionChangedAction;
        }

        private Action<int> _selectionChanged;

        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty( ref isChecked, value );
        }

        private AsyncCommand<int> selectionChangedCommand;
        public AsyncCommand<int> SelectionChangedCommand => selectionChangedCommand ??= new AsyncCommand<int>( SelectionChanged );


        private async Task SelectionChanged( int id )
        {
            _selectionChanged.Invoke( id );
        }
    }
}
