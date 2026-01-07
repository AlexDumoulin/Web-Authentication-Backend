import Register from './Register';
import Login from './Login';
import { Routes, Route } from 'react-router-dom';

function App() {
  return (
    <main className="App">
      <Routes>
        {/* Set Login as the default path */}
        <Route path="login" element={<Login />} />
        <Route path="register" element={<Register />} />
        
        {/* Optional: Redirect root to login */}
        <Route path="/" element={<Login />} /> 
      </Routes>
    </main>
  );
}

export default App;