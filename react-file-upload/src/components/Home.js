import React from "react";
import { Link, Outlet } from "react-router-dom";

const Home = () => {
  return (
    <div>
      <header className="navbar navbar-expand-lg navbar-light bg-light mb-4">
        <div className="container">
          <Link className="navbar-brand" to="/">
            Wilfusr Azure Study
          </Link>
          <div className="navbar-nav">
            <Link className="nav-link" to="/file-uploader">
              File Uploader
            </Link>
            <Link className="nav-link" to="/notification-subscriber">
              Notification Subscriber
            </Link>
          </div>
        </div>
      </header>
      <div className="container">
        <Outlet />
      </div>
    </div>
  );
};

export default Home;