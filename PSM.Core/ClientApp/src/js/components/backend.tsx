import React from "react";

export default class Backend {
  private constructor(name: string) {
    Backend.backends[name] = this;
  }

  private static backends: {} = {};

  public static Get(name: string): Backend {
    return Backend.backends[name] ?? new Backend(name);
  }

  public static readonly Global: Backend = new Backend("global");

  private backendListeners: {} = {};
  private backendState: {} = {};

  private updateBackendValue<T>(valueName: string, value: T) {
    this.backendState[valueName] = value;
    const listeners: React.Component[] = this.backendListeners[valueName];
    if (listeners === undefined) return;
    console.log(listeners);
    listeners.forEach((listener) => { if (listener) { listener.forceUpdate() } });
  }

  private pushListener(valueName: string, listener: React.Component) {
    const listeners: React.Component[] =
      this.backendListeners[valueName] ??
      (this.backendListeners[valueName] = []);
    if (listeners.includes(listener)) return;
    listeners.push(listener);
  }

  public useBackend<T>(
    caller: React.Component,
    variableName: string,
    defaultValue: T|undefined = undefined
  ): [T | undefined, (value: T) => void] {
    const backendValue = this.backendState[variableName] ?? defaultValue;
    if (backendValue === defaultValue)
      this.backendState[variableName] = backendValue;
    this.pushListener(variableName, caller);

    return [
      backendValue,
      (val: T) => this.updateBackendValue<T>(variableName, val)
    ];
  }

  public setBackend<T>(variableName: string, value: T) {
    this.updateBackendValue(variableName, value);
  }
}
