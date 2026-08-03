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

export const login = async (loginData) => {
    const response = await API.post("/Auth/login", loginData);
    return response.data;
};

export const register = async (registerData) => {
    const response = await API.post("/Auth/register", registerData);
    return response.data;
};

export default API;
