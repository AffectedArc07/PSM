import axios from 'axios';
import {notification} from 'antd';

class ApiManager {
  constructor() {
    console.log("[API-M] Starting API manager");
    this.bearer = null;
    this.bearerId = null;
    this.bearerExp = null;
    // We handle our own errors here
    this.axs = axios.create({
      validateStatus: function () {
        return true;
      }
    });
    console.log("[API-M] Ready");
  }

  valid_token() {
    // If we are missing both of these, we fail anyway
    if (!this.bearer || !this.bearerExp) {
      return false;
    }

    // If our token expired, we ain't valid
    if (new Date().getTime() > this.bearerExp) {
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
    this.bearerId = response.data["userId"]
    this.bearerExp = Date.parse(response.data["expiresAt"]);
    this.axs.defaults.headers.common = {
      Authorization: `Bearer${this.bearerId !== 0 ? `(${this.bearerId})` : ``} ${this.bearer}`
    }
    this.reloadState(1);
  }

  setReloadState(s) {
    this.reloadState = s;
  }

  sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  async debug_verify_token() {
    alert("test")
    let res = await this.axs.post("/api/auth/debug_verify")
    if (res.status !== 200)
      alert("failed to validate")
  }
}

export const API = new ApiManager();
