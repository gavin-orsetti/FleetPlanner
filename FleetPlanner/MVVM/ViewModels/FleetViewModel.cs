using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

using MvvmHelpers;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel( Fleet fleet ) : BaseViewModel
    {
        private readonly Fleet fleet = fleet;
        public Fleet Fleet => fleet;

        private int? id;
        public int Id
        {
            get => id ??= Fleet.Id;
            private set => SetProperty( ref id, value );
        }

        private string name;
        public string Name
        {
            get => name ??= Fleet.Name;
            private set => SetProperty( ref name, value );
        }

        private string affiliation;
        public string Affiliation
        {
            get => affiliation ??= Fleet.Affiliation;
            private set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        public string Manifesto
        {
            get => manifesto ??= Fleet.Manifesto;
            private set => SetProperty( ref manifesto, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= Fleet.AreaOfOperation;
            private set => SetProperty( ref areaOfOperation, value );
        }

        private ObservableRangeCollection<TaskUnit> taskUnits;
        public ObservableRangeCollection<TaskUnit> TaskUnits
        {
            get => taskUnits ??= new ObservableRangeCollection<TaskUnit>( Fleet.TaskUnits );
            private set => SetProperty( ref taskUnits, value );
        }
    }
}
