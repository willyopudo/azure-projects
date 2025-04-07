import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";

import "./App.css";
import "bootstrap/dist/css/bootstrap.min.css";

import Home from "./components/Home";
import UploadFiles from "./components/upload-files.component";
import NotificationSubscriber from "./components/NotificationSubscriber";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />}>
          <Route path="file-uploader" element={<UploadFiles />} />
          <Route path="notification-subscriber" element={<NotificationSubscriber />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;