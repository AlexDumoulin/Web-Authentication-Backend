import { useRef, useState, useEffect } from "react";
import { faCheck, faTimes, faInfoCircle } from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import axios from './api/axios';

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

    const [pwd, setPwd] = useState('');
    const [validPwd, setValidPwd] = useState(false);

    const [matchPwd, setMatchPwd] = useState('');
    const [validMatch, setValidMatch] = useState(false);

    const [errMsg, setErrMsg] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    useEffect(() => { firstRef.current.focus(); }, [])
    useEffect(() => { setValidEmail(EMAIL_REGEX.test(email)); }, [email])
    useEffect(() => {
        setValidPwd(PWD_REGEX.test(pwd));
        setValidMatch(pwd === matchPwd);
    }, [pwd, matchPwd])

    useEffect(() => { setErrMsg(''); }, [email, pwd, matchPwd, firstName, lastName])

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsLoading(true);

        const v1 = EMAIL_REGEX.test(email);
        const v2 = PWD_REGEX.test(pwd);
        if (!v1 || !v2 || !validMatch) {
            setErrMsg("Invalid Entry");
            setIsLoading(false);
            return;
        }

        try {
            // Matches your C# CreateUserRequest model
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
            setFirstName(''); setLastName(''); setEmail(''); setPwd(''); setMatchPwd('');
        } catch (err) {
            if (!err?.response) {
                setErrMsg('No Server Response');
            } else if (err.response?.status === 400 || err.response?.status === 409) {
                // Catches your "Email already in use" or "Invalid email format"
                setErrMsg(err.response.data); 
            } else if (err.response?.status === 500) {
                // This will trigger if the C# CreatedAtAction still fails
                setErrMsg('User created, but server response failed. Check your database.');
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
                    <p><a href="#">Sign In</a></p>
                </div>
            ) : (
                <>
                    <p ref={errRef} className={errMsg ? "errmsg" : "offscreen"} aria-live="assertive">{errMsg}</p>
                    <h1>Register</h1>
                    <form onSubmit={handleSubmit}>
                        <label htmlFor="firstname">First Name:</label>
                        <input type="text" id="firstname" ref={firstRef} autoComplete="off" onChange={(e) => setFirstName(e.target.value)} value={firstName} required />

                        <label htmlFor="lastname">Last Name:</label>
                        <input type="text" id="lastname" autoComplete="off" onChange={(e) => setLastName(e.target.value)} value={lastName} required />

                        <label htmlFor="email">
                            Email:
                            <FontAwesomeIcon icon={faCheck} className={validEmail ? "valid" : "hide"} />
                            <FontAwesomeIcon icon={faTimes} className={validEmail || !email ? "hide" : "invalid"} />
                        </label>
                        <input type="email" id="email" onChange={(e) => setEmail(e.target.value)} value={email} required onFocus={() => setEmailFocus(true)} onBlur={() => setEmailFocus(false)} />

                        <label htmlFor="password">
                            Password:
                            <FontAwesomeIcon icon={faCheck} className={validPwd ? "valid" : "hide"} />
                            <FontAwesomeIcon icon={faTimes} className={validPwd || !pwd ? "hide" : "invalid"} />
                        </label>
                        <input type="password" id="password" onChange={(e) => setPwd(e.target.value)} value={pwd} required onFocus={() => setPwdFocus(true)} onBlur={() => setPwdFocus(false)} />

                        <label htmlFor="confirm_pwd">Confirm Password:</label>
                        <input type="password" id="confirm_pwd" onChange={(e) => setMatchPwd(e.target.value)} value={matchPwd} required onFocus={() => setMatchFocus(true)} onBlur={() => setMatchFocus(false)} />

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