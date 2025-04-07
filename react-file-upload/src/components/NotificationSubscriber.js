import React, { useEffect, useState } from "react";
import CryptoJS from "crypto-js";

// Full connection string
const connectionString ="sb_conn";
const subscriptionName = "booknotificationsSub";

// Helper function to parse the connection string
const parseConnectionString = (connectionString) => {
  const parts = connectionString.split(";");
  const result = {};
  parts.forEach((part) => {
    const [key, value] = part.split("=");
    result[key] = value;
  });
  return result;
};

// Helper function to generate a SAS token
const generateSasToken = (resourceUri, keyName, key) => {
  const expiry = Math.floor(Date.now() / 1000) + 3600; // Token valid for 1 hour
  const stringToSign = `${resourceUri}\n${expiry}`;
  const hash = CryptoJS.HmacSHA256(stringToSign, key).toString(CryptoJS.enc.Base64);
  return `SharedAccessSignature sr=${encodeURIComponent(
    resourceUri
  )}&sig=${encodeURIComponent(hash)}&se=${expiry}&skn=${keyName}`;
};

const NotificationSubscriber = () => {
  const [notifications, setNotifications] = useState([]);

  useEffect(() => {
    const receiveMessages = async () => {
      try {
        // Parse the connection string
        const { Endpoint, SharedAccessKeyName, SharedAccessKey, EntityPath } =
          parseConnectionString(connectionString);

        // Construct the resource URI
        const resourceUri = `${Endpoint}${EntityPath}/subscriptions/${subscriptionName}`;

        // Generate the SAS token
        const sasToken = generateSasToken(resourceUri, SharedAccessKeyName, SharedAccessKey);

        // Make the request to receive messages
        const response = await fetch(`${resourceUri}/messages/head`, {
          method: "DELETE", // DELETE is used to receive and remove the message from the queue
          headers: {
            Authorization: sasToken,
            "Content-Type": "application/json",
          },
        });

        if (response.status === 200) {
          const message = await response.json();
          console.log(message);

          setNotifications((prev) => [...prev, message]);
        } else if (response.status === 204) {
          console.log("No messages available.");
        } else {
          console.error("Failed to receive message:", response.status, response.statusText);
        }
      } catch (error) {
        console.error("Error receiving message:", error);
      }
    };

    const interval = setInterval(receiveMessages, 5000); // Poll every 5 seconds

    return () => clearInterval(interval);
  }, []);

  return (
    <div className="p-6">
      <h1 className="text-xl font-bold mb-4">Book Notifications</h1>
      {notifications.map((notification, index) => (
        <div key={index} className="mb-2 p-4 border">
          <p>
            New Book Added: {notification.title} by {notification.author}
          </p>
        </div>
      ))}
    </div>
  );
};

export default NotificationSubscriber;