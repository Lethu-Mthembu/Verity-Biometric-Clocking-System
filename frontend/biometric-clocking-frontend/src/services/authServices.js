import axios from "axios";

console.log("API URL =", import.meta.env.VITE_API_URL);

const API = axios.create({

    baseURL: import.meta.env.VITE_API_URL,
});
export const getApiUrl = (path = "") =>
    `${API.defaults.baseURL.replace(/\/$/, "")}${path.startsWith("/") ? path : `/${path}`}`;

// Attach the logged-in user's token (if any) to every request. Harmless for
// endpoints that don't require auth, and means anything made [Authorize]
// later just works without touching call sites again.
API.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// A token can outlive the deployment that issued it. Clear it when a
// protected request is rejected so the dashboard does not remain open while
// its admin notification requests and stream are silently unauthorized.
API.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401 && localStorage.getItem("token")) {
            localStorage.removeItem("token");
            localStorage.removeItem("userId");
            localStorage.removeItem("role");

            if (["/admin", "/dashboard", "/onboard", "/hr"].includes(window.location.pathname)) {
                window.location.assign("/kiosk");
            }
        }

        return Promise.reject(error);
    }
);

export const login = async (loginData) => {
    const response = await API.post("/Auth/login", loginData);
    return response.data;
};

export const getHrAccountStatus = async () => {
    const response = await API.get("/Auth/hr-account");
    return response.data;
};

export const createHrAccount = async (accountData) => {
    const response = await API.post("/Auth/hr-account", accountData);
    return response.data;
};

export const changeHrPassword = async (passwordData) => {
    const response = await API.post("/Auth/change-password", passwordData);
    return response.data;
};

export default API;
