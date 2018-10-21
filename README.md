`instaface-graphserver` needs a MySQL connection string in an environment variable, e.g.:

```
docker run -p 6542:80 -it danielearwicker/instaface-graphserver -e ConnectionStrings:DefaultConnection="s
erver=192.168.1.170;port=3307;database=instaface1;user=root;password=notverysecret123"
```

The database doesn't have to exist - on startup it checks for it and creates it if necessary (and the tables).

May need to create a user in MySQL tied to the client's IP address and grant it access:

```sql
CREATE USER 'bob'@'192.168.1.160' IDENTIFIED BY 'password123';
GRANT SELECT, INSERT, UPDATE, DELETE on instaface1.* to 'bob'@'192.168.1.160';
FLUSH PRIVILEGES;
```
