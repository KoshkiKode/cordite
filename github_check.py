import urllib.request
url = 'https://api.github.com/repos/KoshkiKode/the-cordite-wars/actions/jobs/69952166029/logs'
try:
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req) as res:
        log_text = res.read().decode('utf-8')
        lines = log_text.splitlines()
        print('\n'.join(lines[-30:]))
except Exception as e:
    print('Failed:', str(e))
