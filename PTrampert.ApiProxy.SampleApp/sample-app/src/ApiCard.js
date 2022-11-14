import Card from 'react-bootstrap/Card';
import {useEffect, useState} from "react";

async function callApi(setResponse) {
  try {
    let response = await fetch('/api/SampleApi/AuthEcho');
    setResponse(await response.json());
  } catch (e) {
    setResponse(e);
  }
}

export default function ApiCard() {
  let [apiResponse, setApiResponse] = useState({});

  useEffect(() => {
    callApi(setApiResponse);
  });

  return (
    <Card style={{width: '18rem'}}>
      <Card.Header>
        API Proxy Test
      </Card.Header>
      <Card.Body>
        <pre>
          {JSON.stringify(apiResponse, null, 4) || "No Response"}
        </pre>
      </Card.Body>
    </Card>
  );
}