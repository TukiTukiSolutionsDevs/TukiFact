import requests

BASE_URL = "http://localhost:5186"
TIMEOUT = 30

def test_health_check_all_dependencies():
    url = f"{BASE_URL}/health"
    try:
        response = requests.get(url, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Request to {url} failed with exception: {e}"

    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"

    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert 'status' in data, "'status' field missing from response JSON"
    assert data['status'] == 'Healthy', f"Expected 'status'='Healthy', got '{data['status']}'"

    assert 'checks' in data, "'checks' field missing from response JSON"
    checks = data['checks']
    assert isinstance(checks, list), "'checks' field is not a list"
    assert len(checks) == 3, f"Expected 3 checks, got {len(checks)}"

    required_check_names = {'postgresql', 'nats', 'minio'}
    found_check_names = set()

    for check in checks:
        assert isinstance(check, dict), f"Each check should be a dict, got {type(check)}"
        # Validate presence and types of fields
        for field in ['name', 'status', 'duration', 'tags']:
            assert field in check, f"Field '{field}' missing in a check"

        # Validate name is lowercase string and one of expected
        name = check['name']
        assert isinstance(name, str), f"Check 'name' is not a string: {type(name)}"
        name_lower = name.lower()
        assert name_lower in required_check_names, f"Unexpected check name '{name}'"
        found_check_names.add(name_lower)

        # Validate status is 'Healthy'
        status = check['status']
        assert isinstance(status, str), f"Check 'status' is not a string: {type(status)}"
        assert status == 'Healthy', f"Check '{name}' status expected 'Healthy', got '{status}'"

        # Validate duration is number (int or float)
        duration = check['duration']
        assert isinstance(duration, (int, float)), f"Check 'duration' is not a number: {type(duration)}"

        # Validate tags is a list
        tags = check['tags']
        assert isinstance(tags, list), f"Check 'tags' is not a list: {type(tags)}"

    missing_checks = required_check_names - found_check_names
    assert not missing_checks, f"Missing expected checks: {missing_checks}"

test_health_check_all_dependencies()