using SQLite;

namespace FleetPlanner.MVVM.Models
{
    public interface IStorable
    {
        [PrimaryKey, AutoIncrement]
        int Id
        {
            get; set;
        }

    }
}