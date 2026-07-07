import urllib.request, json, uuid, datetime, sys

BASE = 'http://localhost:5178'

def post(path, data, token=None):
    url = BASE + path
    b = json.dumps(data).encode()
    headers = {'Content-Type':'application/json'}
    if token: headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(url, data=b, headers=headers, method='POST')
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.load(r)
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()

# 1) Register
email = f'e2e-employer+{uuid.uuid4().hex[:6]}@example.com'
reg = {
    'email': email,
    'password': 'Pass123!',
    'firstName': 'E2E',
    'lastName': 'Employer',
    'role': 'employer'
}
status, resp = post('/api/v1/auth/register', reg)
print('REGISTER', status)
print(json.dumps(resp, indent=2))
if status != 200:
    sys.exit(1)

token = resp['accessToken']

# 2) Create Workshop
start = (datetime.datetime.utcnow() + datetime.timedelta(days=1)).isoformat() + 'Z'
end = (datetime.datetime.utcnow() + datetime.timedelta(days=2)).isoformat() + 'Z'
work = {
    'title': 'E2E Workshop',
    'description': 'Test workshop',
    'coverImageUrl': None,
    'price': 0,
    'capacity': 10,
    'locationType': 'online',
    'locationDetail': 'Zoom',
    'startAt': start,
    'endAt': end,
    'tags': [],
}
status, resp = post('/api/v1/workshops', work, token)
print('CREATE_WORKSHOP', status)
print(json.dumps(resp, indent=2) if isinstance(resp, dict) else resp)
if status not in (200,201):
    sys.exit(1)
workshop_id = resp.get('id')

# 3) Create Post
post_body = {
    'workshopId': workshop_id,
    'caption': 'E2E test post',
    'tagSlugs': []
}
status, resp = post('/api/v1/posts', post_body, token)
print('CREATE_POST', status)
print(json.dumps(resp, indent=2) if isinstance(resp, dict) else resp)

