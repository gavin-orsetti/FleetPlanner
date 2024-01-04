using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FleetPlanner.MVVM.Models;

namespace FleetPlanner.Services
{
    public interface IReadOnlyDatabaseService
    {
        public IDatabaseItem GetRow( int id );
        //public List<IDatabaseItem> GetRange( IEnumerable<int> rows );
        //public List<IDatabaseItem> GetAll();
    }
}
