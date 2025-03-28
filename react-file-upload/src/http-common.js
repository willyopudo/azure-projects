import axios from "axios";

export default axios.create({
  baseURL: "https://fileuploadappservice01001.azurewebsites.net/api",
  headers: {
    "Content-type": "application/json",
    'Access-Control-Allow-Origin': '*', // Be cautious with this in production
  }
});

