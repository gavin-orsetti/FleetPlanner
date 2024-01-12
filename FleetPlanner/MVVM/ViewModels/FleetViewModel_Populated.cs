using FleetPlanner.MVVM.Models;

using MvvmHelpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetPlanner.MVVM.ViewModels
{
    public class FleetViewModel_Populated( Fleet fleet ) : FleetViewModel
    {
        private readonly Fleet fleet = fleet;
        new public Fleet Fleet => fleet;

        private int? id;
        new public int Id
        {
            get => id ??= Fleet.Id;
            private set => SetProperty( ref id, value );
        }

        private string name;
        new public string Name
        {
            get => name ??= Fleet.Name;
            private set => SetProperty( ref name, value );
        }

        private string affiliation;
        new public string Affiliation
        {
            get => affiliation ??= Fleet.Affiliation;
            private set => SetProperty( ref affiliation, value );
        }

        private string manifesto;
        new public string Manifesto
        {
            get => manifesto ??= Fleet.Manifesto;
            private set => SetProperty( ref manifesto, value );
        }

        public string Manifesto_150
        {
            get => Manifesto != string.Empty && Manifesto.Length > 150 ? Manifesto[ ..150 ].Trim() : Manifesto;
        }

        private string areaOfOperation;
        new public string AreaOfOperation
        {
            get => areaOfOperation ??= Fleet.AreaOfOperation;
            private set => SetProperty( ref areaOfOperation, value );
        }

        private ObservableRangeCollection<TaskUnit> taskUnits;
        new public ObservableRangeCollection<TaskUnit> TaskUnits
        {
            get => taskUnits ??= [ .. Fleet.TaskUnits ];
            private set => SetProperty( ref taskUnits, value );
        }
    }
}
