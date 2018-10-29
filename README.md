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


## MySQL Replication

You can a MySQL instance enabled for replication in Docker with:

```
docker run -p 3361:3306 --name mysqlD -e MYSQL_ROOT_PASSWORD=P@ssw0rd -d mysql:latest --server-id=3361 --gtid-mode=ON --enforce-gtid-consistency=TRUE
```

You can start a second instance running on another port and with another server-id (for no particular reason I've used the same value for both options).

Then in one of them (say, 3361) issue the commands:

```
CHANGE MASTER TO
  MASTER_HOST = '<your-ip-address>',
  MASTER_PORT = 3362,
  MASTER_USER = 'root',
  MASTER_PASSWORD = 'P@ssw0rd',
  MASTER_AUTO_POSITION = 1;

START SLAVE;  
```

Note the port is of the other instance, which will be the master. On the slave you can say:

```  
SHOW SLAVE STATUS;
```

to see if anything is erroring.

On the other instance, run:

```
SHOW SLAVE HOSTS
```

and you should eventually see the slave instance listed.

At this point, any changes you make at the master should automatically appear at the slave.
