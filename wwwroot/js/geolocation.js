// Geolocation helper functions
window.geolocationHelper = {
    getCurrentPosition: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error("Geolocation is not supported by this browser"));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                (error) => {
                    // Return null on error so app can use default location
                    console.log("Geolocation error:", error.message);
                    resolve(null);
                },
                {
                    enableHighAccuracy: false,
                    timeout: 5000,
                    maximumAge: 300000 // 5 minutes cache
                }
            );
        });
    }
};
