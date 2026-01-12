import axios from '../api/axios';
import { useContext } from 'react';
import AuthContext from '../context/AuthProvider';

const useRefreshToken = () => {
    const { setAuth } = useContext(AuthContext);

    const refresh = async () => {
        const response = await axios.get('/Auth/refresh', {
            withCredentials: true // Sends the HttpOnly cookie automatically
        });
        
        setAuth(prev => {
            return { 
                ...prev, 
                accessToken: response.data.accessToken 
            }
        });
        return response.data.accessToken;
    }
    return refresh;
};

export default useRefreshToken;