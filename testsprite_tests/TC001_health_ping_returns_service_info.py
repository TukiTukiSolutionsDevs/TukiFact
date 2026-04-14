import requests

def test_health_ping_returns_service_info():
    url = "http://localhost:80/api/ping"
    timeout = 30
    try:
        response = requests.get(url, timeout=timeout)
        assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"
        json_data = response.json()
        # Check for required fields presence
        required_fields = ['service', 'version', 'environment', 'timestamp']
        for field in required_fields:
            assert field in json_data, f"Field '{field}' missing from response JSON"
        # Validate 'service' field exact value
        assert json_data['service'] == "TukiFact API", f"Expected service to be 'TukiFact API', got '{json_data['service']}'"
        # Validate 'version' is a non-empty string
        version = json_data['version']
        assert isinstance(version, str) and version.strip() != "", "'version' must be a non-empty string"
    except requests.RequestException as e:
        assert False, f"RequestException occurred: {e}"

test_health_ping_returns_service_info()