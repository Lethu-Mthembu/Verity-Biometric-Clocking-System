const geocodingApiUrl = import.meta.env.VITE_GEOCODING_API_URL?.trim();

export async function getLocationName(latitude, longitude) {
    if (!geocodingApiUrl) {
        throw new Error("VITE_GEOCODING_API_URL is not configured.");
    }

    const requestUrl = new URL(geocodingApiUrl);
    requestUrl.searchParams.set("format", "json");
    requestUrl.searchParams.set("lat", String(latitude));
    requestUrl.searchParams.set("lon", String(longitude));

    const response = await fetch(requestUrl, {
        headers: {
            Accept: "application/json"
        }
    });

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
