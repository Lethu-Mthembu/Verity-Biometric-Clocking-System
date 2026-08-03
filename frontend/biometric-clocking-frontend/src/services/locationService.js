export async function getLocationName(latitude, longitude) {
    const response = await fetch(
        `https://nominatim.openstreetmap.org/reverse?format=json&lat=${latitude}&lon=${longitude}`,
        {
            headers: {
                Accept: "application/json"
            }
        }
    );

    if (!response.ok) {
        throw new Error("Unable to get location.");
    }

    const data = await response.json();

    return data.display_name;
}

export function getCurrentLocation() {
    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            (position) => resolve(position.coords),
            (error) => reject(error),
            {
                enableHighAccuracy: true
            }
        );
    });
}