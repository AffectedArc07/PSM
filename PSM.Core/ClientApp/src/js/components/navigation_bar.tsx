import React from "react";

/// NavigationButton type, navbar expects all names to be unique
export type NavigationButton = {
  name: string
  callback: () => void
}

type NavigationBarProps = {
  buttons: NavigationButton[]
  selected_button: NavigationButton | undefined
  disabled: boolean | undefined
}

type NavigationBarState = {
  active_button: string | undefined
}

export default class NavigationBar extends React.Component<NavigationBarProps, NavigationBarState> {
  render() {
    if (!this.props.buttons || this.props.buttons.length === 0) {
      return (<p>No Navigation Elements</p>)
    }
    const active_button = this.state?.active_button || this.props.selected_button?.name
    console.log(`Rendering. state: '${this.state?.active_button}' props: '${this.props.selected_button}'`)
    console.log(`Active button: ${active_button}`)
    return (
      <div>
        {this.props.buttons.map(button => (
          <button key={button.name}
                  onClick={() => {
                    if (button.name === active_button) {
                      console.log(`attempting to click already active button`)
                      return
                    }
                    button.callback();
                    this.setState({active_button: button.name})
                  }}
                  disabled={this.props.disabled || button.name === active_button}
                  color={button.name === active_button ? '#f00' : '#00f'}>
            {button.name}
          </button>
        ))}
      </div>
    );
  }
}
