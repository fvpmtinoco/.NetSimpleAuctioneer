namespace NetSimpleAuctioneer.API.Features.Vehicles.Shared
{
    public interface IVehicleService
    {
        /// <summary>
        /// Check if the vehicle year is not above current year
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        bool IsVehicleYearValid(int year);
    }
    public class VehicleService : IVehicleService
    {
        public bool IsVehicleYearValid(int year)
        {
            return year <= DateTime.Now.Year;
        }
    }
}
