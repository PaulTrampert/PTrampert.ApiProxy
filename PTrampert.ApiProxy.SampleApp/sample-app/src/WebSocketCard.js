import Card from 'react-bootstrap/Card';
import Form from 'react-bootstrap/Form';
import Button from 'react-bootstrap/Button';
import {Component} from "react";

export default class WebSocketCard extends Component {

  constructor(props) {
    super(props);
    
    this.state = {
      message: "",
      response: ""
    };
  }
  
  componentDidMount = () => {
    this.socket = new WebSocket("ws://localhost:8080/api/SampleApi/ws-echo");
    this.socket.onmessage = (event) => {
      this.setState({response: event.data});
    }
  }
  
  componentWillUnmount = () => {
    this.socket.close();
  }
  
  onSend = (e) => {
    let {message} = this.state;
    e.preventDefault();
    this.socket.send(message);
  }
  
  render() {
    let {
      message,
      response
    } = this.state;
    
    return (
      <Card>
        <Card.Header>
          WebSocket Proxy Test
        </Card.Header>
        <Card.Body>
          <Form onSubmit={(e) => {
            e.preventDefault();
            this.socket.send(message)
          }}>
            <Form.Group>
              <Form.Label>Message</Form.Label>
              <Form.Control type="text" value={message} onChange={e => this.setState({message: e.target.value})}/>
            </Form.Group>
            <Button type="submit">Send</Button>
          </Form>
          <h5>Response From Server </h5>
          <pre>
            {response}
          </pre>
        </Card.Body>
      </Card>
    );
  }
}