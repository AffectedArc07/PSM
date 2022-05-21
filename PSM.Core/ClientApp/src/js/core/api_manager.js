import axios from 'axios';
import { notification } from 'antd';

class ApiManager {
  constructor() {
    console.log("[APIM] Starting API manager");
    this.bearer = null;
    this.expiration_date = null;
    // We handle our own errors here
    this.axs = axios.create({ validateStatus: function (status) { return true; } });
    console.log("[APIM] Ready");
  }

  valid_token() {
    // If we are missing both of these, we fail anyway
    if (!this.bearer || !this.expiration_date) {
      return false;
    }

    // If our token expired, we aint valid
    if (new Date().getTime() > this.expiration_date) {
      return false;
    }

    // Otherwise, we are!
    return true;
  }

  async attempt_login(form) {
    let response = await this.axs.post("/api/auth/login", {}, {
      auth: {
        username: form["username"],
        password: form["password"],
      },
    });

    if (response.status !== 200) {
      notification.open({
        message: "Error - " + response.status,
        description: response.data,
      });
      // Make them wait a second
      await this.sleep(1000);
      return false;
    }

    // If we are here we have our data
    this.bearer = response.data["token"];
    this.expiration_date = Date.parse(response.data["expiresAt"]);
    this.reloadState(1);
  }

  setReloadState(s) {
    this.reloadState = s;
  }

  sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }


}

export const API = new ApiManager();
