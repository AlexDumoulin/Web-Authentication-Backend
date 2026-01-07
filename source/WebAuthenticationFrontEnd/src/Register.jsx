import { useRef, useState, useEffect } from "react";
import { faCheck, faTimes, faInfoCircle } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import axios from './api/axios';
import { Link } from 'react-router-dom';

const PWD_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%]).{8,24}$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const REGISTER_URL = '/Auth/user';

const Register = () => {
    const firstRef = useRef();
    const errRef = useRef();

    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');

    const [email, setEmail] = useState('');
    const [validEmail, setValidEmail] = useState(false);
    const [emailFocus, setEmailFocus] = useState(false);

    const [pwd, setPwd] = useState('');
    const [validPwd, setValidPwd] = useState(false);
    const [pwdFocus, setPwdFocus] = useState(false);

    const [matchPwd, setMatchPwd] = useState('');
    const [validMatch, setValidMatch] = useState(false);
    const [matchFocus, setMatchFocus] = useState(false);

    const [errMsg, setErrMsg] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    // Focus on first input on load
    useEffect(() => {
        firstRef.current.focus();
    }, [])

    // Validate email
    useEffect(() => {
        setValidEmail(EMAIL_REGEX.test(email));
    }, [email])

    // Validate password and match
    useEffect(() => {
        setValidPwd(PWD_REGEX.test(pwd));
        setValidMatch(pwd === matchPwd);
    }, [pwd, matchPwd])

    // Clear error message when user changes inputs
    useEffect(() => {
        setErrMsg('');
    }, [email, pwd, matchPwd, firstName, lastName])

    const handleSubmit = async (e) => {
        e.preventDefault();
        
        // Final validation check
        const v1 = EMAIL_REGEX.test(email);
        const v2 = PWD_REGEX.test(pwd);
        if (!v1 || !v2 || !validMatch) {
            setErrMsg("Invalid Entry");
            return;
        }

        setIsLoading(true);

        try {
            const payload = {
                FirstName: firstName,
                LastName: lastName,
                Email: email,
                Password: pwd,
                TwoFaKey: "", 
                TwoFaUri: ""
            };

            await axios.post(REGISTER_URL, payload, {
                headers: { 'Content-Type': 'application/json' },
                withCredentials: true
            });

            setSuccess(true);
            // Clear inputs
            setFirstName('');
            setLastName('');
            setEmail('');
            setPwd('');
            setMatchPwd('');
        } catch (err) {
            if (!err?.response) {
                setErrMsg('No Server Response');
            } else if (err.response?.status === 400 || err.response?.status === 409) {
                setErrMsg(err.response.data); 
            } else if (err.response?.status === 500) {
                setErrMsg('User created, but server response failed.');
            } else {
                setErrMsg('Registration Failed');
            }
            errRef.current.focus();
        } finally {
            setIsLoading(false);
        }
    }

    return (
        <section>
            {success ? (
                <div className="success-container">
    <h1>Success!</h1>
    <p>Your account has been created.</p>
    <p>
        <Link to="/login">Sign In</Link>
    </p>
</div>
            ) : (
                <>
                    <p ref={errRef} className={errMsg ? "errmsg" : "offscreen"} aria-live="assertive">{errMsg}</p>
                    <h1>Register</h1>
                    <form onSubmit={handleSubmit}>
                        
                        {/* First Name */}
                        <label htmlFor="firstname">First Name:</label>
                        <input 
                            type="text" 
                            id="firstname" 
                            ref={firstRef} 
                            autoComplete="off" 
                            onChange={(e) => setFirstName(e.target.value)} 
                            value={firstName} 
                            required 
                        />

                        {/* Last Name */}
                        <label htmlFor="lastname">Last Name:</label>
                        <input 
                            type="text" 
                            id="lastname" 
                            autoComplete="off" 
                            onChange={(e) => setLastName(e.target.value)} 
                            value={lastName} 
                            required 
                        />

                        {/* Email */}
                        <label htmlFor="email">
                            Email:
                            <FontAwesomeIcon icon={faCheck} className={validEmail ? "valid" : "hide"} />
                            <FontAwesomeIcon icon={faTimes} className={validEmail || !email ? "hide" : "invalid"} />
                        </label>
                        <input 
                            type="email" 
                            id="email" 
                            onChange={(e) => setEmail(e.target.value)} 
                            value={email} 
                            required 
                            aria-invalid={validEmail ? "false" : "true"}
                            aria-describedby="emailnote"
                            onFocus={() => setEmailFocus(true)} 
                            onBlur={() => setEmailFocus(false)} 
                        />
                        <p id="emailnote" className={emailFocus && email && !validEmail ? "instructions" : "offscreen"}>
                            <FontAwesomeIcon icon={faInfoCircle} />
                            Please enter a valid email address format.
                        </p>

                        {/* Password */}
                        <label htmlFor="password">
                            Password:
                            <FontAwesomeIcon icon={faCheck} className={validPwd ? "valid" : "hide"} />
                            <FontAwesomeIcon icon={faTimes} className={validPwd || !pwd ? "hide" : "invalid"} />
                        </label>
                        <input 
                            type="password" 
                            id="password" 
                            onChange={(e) => setPwd(e.target.value)} 
                            value={pwd} 
                            required 
                            aria-invalid={validPwd ? "false" : "true"}
                            aria-describedby="pwdnote"
                            onFocus={() => setPwdFocus(true)} 
                            onBlur={() => setPwdFocus(false)} 
                        />
                        <p id="pwdnote" className={pwdFocus && !validPwd ? "instructions" : "offscreen"}>
                            <FontAwesomeIcon icon={faInfoCircle} />
                            8 to 24 characters.<br />
                            Must include uppercase and lowercase letters, a number, and a special character (! @ # $ %).
                        </p>

                        {/* Confirm Password */}
                        <label htmlFor="confirm_pwd">
                            Confirm Password:
                            <FontAwesomeIcon icon={validMatch && matchPwd ? "valid" : "hide"} />
                            <FontAwesomeIcon icon={validMatch || !matchPwd ? "hide" : "invalid"} />
                        </label>
                        <input 
                            type="password" 
                            id="confirm_pwd" 
                            onChange={(e) => setMatchPwd(e.target.value)} 
                            value={matchPwd} 
                            required 
                            aria-invalid={validMatch ? "false" : "true"}
                            aria-describedby="confirmnote"
                            onFocus={() => setMatchFocus(true)} 
                            onBlur={() => setMatchFocus(false)} 
                        />
                        <p id="confirmnote" className={matchFocus && !validMatch ? "instructions" : "offscreen"}>
                            <FontAwesomeIcon icon={faInfoCircle} />
                            Must match the first password input field.
                        </p>

                        <button disabled={!validEmail || !validPwd || !validMatch || !firstName || !lastName || isLoading}>
                            {isLoading ? "Processing..." : "Sign Up"}
                        </button>
                    </form>
                </>
            )}
        </section>
    );
}

export default Register;