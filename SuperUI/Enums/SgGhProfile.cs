namespace SuperUI.Enums;

/// <summary>GraphHopper routing vehicle profile.</summary>
public enum SgGhProfile
{
    /// <summary>Shortest distance.</summary>
    Car = 0,
    /// <summary>Fastest route for cars.</summary>
    CarFast = 1,
    /// <summary>Bicycle route.</summary>
    Bike = 2,
    /// <summary>Mountain bike route.</summary>
    BikeMtb = 3,
    /// <summary>Racing bike route.</summary>
    BikeRoad = 4,
    /// <summary>E-bike route.</summary>
    BikeElectric = 5,
    /// <summary>Foot / hiking route.</summary>
    Foot = 6,
    /// <summary>Hiking route.</summary>
    Hike = 7,
    /// <summary>Motorcycle route.</summary>
    Motorcycle = 8,
    /// <summary>Heavy vehicle / truck route.</summary>
    Truck = 9,
    /// <summary>Small car / city vehicle route.</summary>
    SmallTruck = 10,
    /// <summary>Delivery vehicle route.</summary>
    Delivery = 11,
    /// <summary>Emergency vehicle route (ambulance / fire engine).</summary>
    Emergency = 12,
    /// <summary>Kick-scooter / micromobility routing.</summary>
    Scooter = 13
}
