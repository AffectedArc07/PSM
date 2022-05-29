import React from "react";
import Backend from "./backend";

export default class NavigationBar extends React.Component {
  render() {
    const [navTab, setNavTab] = Backend.Global.useBackend(this, "navTab", 0);
    const [navTabs] = Backend.Global.useBackend<string[]>(this, "navTabs", []);
    if (navTabs === undefined) {
      console.error("No nav tabs for global nav bar")
      return "Failed to load NavBar";
    }
    return (
      <div id="navbar">
        <ul>
          {navTabs.map((tab) => (
            <li
              id={navTab === navTabs.indexOf(tab) ? "selected" : "notSelected"}
              onClick={() => setNavTab(navTabs.indexOf(tab))}
              key={tab}
            >
              {tab}
            </li>
          ))}
        </ul>
      </div>
    );
  }
}
