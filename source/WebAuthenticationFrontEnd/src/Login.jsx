import { useRef, useState, useEffect, useContext } from 'react';
import AuthContext from "./context/AuthProvider";
import axios from './api/axios';
import { Link } from 'react-router-dom';

const LOGIN_URL = '/Auth/login';

const Login = () => {
    const { setAuth } = useContext(AuthContext);
    const userRef = useRef();
    const errRef = useRef();

    const [email, setEmail] = useState('');
    const [pwd, setPwd] = useState('');
    const [errMsg, setErrMsg] = useState('');
    const [success, setSuccess] = useState(false);
    
    // NEW: Loading state
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        userRef.current.focus();
    }, [])

    useEffect(() => {
        setErrMsg('');
    }, [email, pwd])

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true); // Start loading

        try {
            const response = await axios.post(LOGIN_URL,
                { 
                    Email: email, 
                    Password: pwd 
                },
                {
                    headers: { 'Content-Type': 'application/json' },
                    withCredentials: true 
                }
            );

            const accessToken = response?.data?.accessToken;
            setAuth({ email, accessToken });
            
            setEmail('');
            setPwd('');
            setSuccess(true);
        } catch (err) {
            if (!err?.response) {
                setErrMsg('No Server Response');
            } else if (err.response?.status === 400) {
                setErrMsg('Missing Email or Password');
            } else if (err.response?.status === 401) {
                setErrMsg('Unauthorized: Invalid Email or Password');
            } else {
                setErrMsg('Login Failed');
            }
            errRef.current.focus();
        } finally {
            setIsLoading(false); // End loading regardless of success/fail
        }
    }

    return (
        <>
            {success ? (
                <section>
                    <h1>You are logged in!</h1>
                    <br />
                    <p>
                        <Link to="/">Go to Home</Link>
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
                            disabled={isLoading} // Disable input while loading
                        />

                        <label htmlFor="password">Password:</label>
                        <input
                            type="password"
                            id="password"
                            onChange={(e) => setPwd(e.target.value)}
                            value={pwd}
                            required
                            disabled={isLoading} // Disable input while loading
                        />
                        
                        {/* Button changes appearance based on loading state */}
                        <button disabled={isLoading}>
                            {isLoading ? "Signing In..." : "Sign In"}
                        </button>
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