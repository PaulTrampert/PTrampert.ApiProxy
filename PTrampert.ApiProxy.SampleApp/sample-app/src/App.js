import Container from 'react-bootstrap/Container';
import Navbar from 'react-bootstrap/Navbar';

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
    </div>
  );
}

export default App;
