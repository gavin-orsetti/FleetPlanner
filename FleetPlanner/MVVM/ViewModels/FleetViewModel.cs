using FleetPlanner.MVVM.Models;

using MvvmHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel : BaseViewModel
    {
        public FleetViewModel()
        {
            fleet = new Fleet();
        }

        readonly Fleet fleet;
        public Fleet Fleet => fleet;

        private int? id;
        public int Id
        {
            get => id ??= 0;
            set => SetProperty( ref id, value );
        }

        private string name;
        public string Name
        {
            get => name ??= string.Empty;
            set => SetProperty( ref name, value );
        }

        private string affiliation;
        public string Affiliation
        {
            get => affiliation ??= string.Empty;
            set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        public string Manifesto
        {
            get => manifesto ??= string.Empty;
            set => SetProperty( ref manifesto, value );
        }

        private string areaOfOperation;
        public string AreaOfOperation
        {
            get => areaOfOperation ??= string.Empty;
            set => SetProperty( ref areaOfOperation, value );
        }

        private ObservableRangeCollection<TaskUnit> taskUnits;
        public ObservableRangeCollection<TaskUnit> TaskUnits
        {
            get => taskUnits ??= [];
            set => SetProperty( ref taskUnits, value );
        }
    }
}
