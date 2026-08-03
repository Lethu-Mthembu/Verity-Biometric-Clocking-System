import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { login } from "../../../services/authServices";
import "../../../styles/login.css";

function Login() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    email: "",
    password: "",
  });

  const handleChange = (e) => {
    setForm({
      ...form,
      [e.target.name]: e.target.value,
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      const result = await login(form);



      alert("Login successful!");

      if (result.role === "Admin") {
        navigate("/admin");
      } else if (result.role === "HR") {
        navigate("/hr");
      } else {
        navigate("/dashboard");
      }
    } catch (err) {
      alert("Invalid email or password.");
      console.error(err);
    }
  };



  return (
    <div className="login-container">
      <div className="login-card">

        <h1>Biometric Attendance</h1>
        <h3>Clocking System</h3>

        <form onSubmit={handleSubmit}>

          <input
            type="email"
            name="email"
            placeholder="Email Address"
            value={form.email}
            onChange={handleChange}
            required
          />

          <input
            type="password"
            name="password"
            placeholder="Password"
            value={form.password}
            onChange={handleChange}
            required
          />
          <button type="submit">
            Login
          </button>

        </form>

        <p>
          Don't have an account?
          <Link to="/register"> Register</Link>
        </p>

      </div>
    </div>
  );
}

export default Login;