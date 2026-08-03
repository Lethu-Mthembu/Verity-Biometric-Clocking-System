import API from "./authServices";

export const getEmployees = async () => {
    const response = await API.get("/Employee");
    return response.data;
};

export const getEmployee = async (employeeNumber) => {
    const response = await API.get(
        `/Employee/number/${employeeNumber}`
    );
    return response.data;
};

export const createEmployee = async (employeeData) => {
    const response = await API.post(
        "/Employee",
        employeeData
    );
    return response.data;
};

export const updateEmployee = async (
    employeeNumber,
    employeeData
) => {
    const response = await API.put(
        `/Employee/${employeeNumber}`,
        employeeData
    );
    return response.data;
};

export const deleteEmployee = async (employeeNumber) => {
    const response = await API.delete(
        `/Employee/${employeeNumber}`
    );
    return response.data;
};