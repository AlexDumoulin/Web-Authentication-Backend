import { useRef, useState, useEffect, useContext } from 'react';
import AuthContext from "./context/AuthProvider";
import axios from './api/axios';
import { Link } from 'react-router-dom';

// Matches the endpoint shown in your Postman screenshot
const LOGIN_URL = '/Auth/login';

const Login = () => {
    const { setAuth } = useContext(AuthContext);
    const userRef = useRef();
    const errRef = useRef();

    const [email, setEmail] = useState('');
    const [pwd, setPwd] = useState('');
    const [errMsg, setErrMsg] = useState('');
    const [success, setSuccess] = useState(false);

    // Focus on the first input on load
    useEffect(() => {
        userRef.current.focus();
    }, [])

    // Clear error messages when user types
    useEffect(() => {
        setErrMsg('');
    }, [email, pwd])

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            const response = await axios.post(LOGIN_URL,
                // Matches your C# LoginRequest model keys
                { 
                    Email: email, 
                    Password: pwd 
                },
                {
                    headers: { 'Content-Type': 'application/json' },
                    withCredentials: true
                }
            );

            /* The backend now returns a JSON object like:
               { accessToken: "ey...", email: "user@example.com" }
            */
            const accessToken = response?.data?.accessToken;
            
            // Save to AuthContext
            setAuth({ email, accessToken });
            
            // Clear inputs and show success
            setEmail('');
            setPwd('');
            setSuccess(true);
        } catch (err) {
    console.log("Full Error Object:", err); // ADD THIS TO SEE THE CULPRIT

    if (!err?.response) {
        setErrMsg('No Server Response - Check if a browser extension is intercepting the call.');
    } else if (err.response?.status === 400) {
        setErrMsg('Missing Email or Password');
    } else if (err.response?.status === 401) {
        setErrMsg(err.response.data || 'Unauthorized');
    } else {
        setErrMsg('Login Failed');
    }
    errRef.current.focus();
}
    }

    return (
        <>
            {success ? (
                <section>
                    <h1>You are logged in!</h1>
                    <br />
                    <p>
                        <a href="#">Go to Home</a>
                    </p>
                </section>
            ) : (
                <section>
                    <p 
                        ref={errRef} 
                        className={errMsg ? "errmsg" : "offscreen"} 
                        aria-live="assertive"
                    >
                        {errMsg}
                    </p>

                    <h1>Sign In</h1>
                    <form onSubmit={handleSubmit}>
                        <label htmlFor="email">Email:</label>
                        <input
                            type="email"
                            id="email"
                            ref={userRef}
                            autoComplete="off"
                            onChange={(e) => setEmail(e.target.value)}
                            value={email}
                            required
                        />

                        <label htmlFor="password">Password:</label>
                        <input
                            type="password"
                            id="password"
                            onChange={(e) => setPwd(e.target.value)}
                            value={pwd}
                            required
                        />
                        
                        <button>Sign In</button>
                    </form>

                    <p>
                        Need an Account?<br />
                        <span className="line">
                            <Link to="/register">Sign Up</Link>
                        </span>
                    </p>
                </section>
            )}
        </>
    )
}

export default Login;