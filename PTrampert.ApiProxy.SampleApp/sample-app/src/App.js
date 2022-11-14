import Container from 'react-bootstrap/Container';
import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import Navbar from 'react-bootstrap/Navbar';
import ApiCard from "./ApiCard";
import WebSocketCard from "./WebSocketCard";


function App() {
  return (
    <div className="App">
      <header>
        <Navbar bg="dark" variant="dark">
          <Container>
            <Navbar.Brand>PTrampert.ApiProxy</Navbar.Brand>
          </Container>
        </Navbar>
      </header>
      <main>
        <Container>
          <Row>
            <Col>
              <ApiCard />
            </Col>
            <Col>
              <WebSocketCard />
            </Col>
          </Row>
        </Container>
      </main>
    </div>
  );
}

export default App;
