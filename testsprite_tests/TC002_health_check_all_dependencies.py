import requests

def test_health_check_all_dependencies():
    url = "http://localhost:80/health"
    timeout = 30
    try:
        response = requests.get(url, timeout=timeout)
    except requests.RequestException as e:
        assert False, f"HTTP request failed: {e}"

    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"

    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert 'status' in data, "'status' field not in response JSON"
    assert data['status'] == 'Healthy', f"Expected status 'Healthy', got '{data['status']}'"

    assert 'checks' in data, "'checks' field not in response JSON"
    checks = data['checks']
    assert isinstance(checks, list), "'checks' should be a list"
    assert len(checks) == 3, f"Expected 3 checks, got {len(checks)}"

    expected_names = {'postgresql', 'nats', 'minio'}
    found_names = set()
    for check in checks:
        assert isinstance(check, dict), "Each check should be a dict"
        assert 'name' in check and isinstance(check['name'], str), "Check missing 'name' or 'name' not string"
        check_name = check['name'].lower()
        assert check_name in expected_names, f"Unexpected check name '{check_name}'"
        found_names.add(check_name)

        assert 'status' in check and isinstance(check['status'], str), "Check missing 'status' or 'status' not string"
        assert check['status'] == 'Healthy', f"Check '{check_name}' has status '{check['status']}', expected 'Healthy'"

        assert 'duration' in check, "Check missing 'duration'"
        duration = check['duration']
        assert isinstance(duration, (int, float)), f"Check '{check_name}' duration is not a number"

        assert 'tags' in check, "Check missing 'tags'"
        tags = check['tags']
        assert isinstance(tags, list), f"Check '{check_name}' tags is not a list"

    assert found_names == expected_names, f"Checks names found {found_names} don't match expected {expected_names}"

test_health_check_all_dependencies()