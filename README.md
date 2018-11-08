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

## Multiple GraphServer instances

Each GraphServer instance is configured with the public address it listens on, and the address of the shared Redis. On startup it registers its address on Redis and begins functioning as a follower.

All graphservers have a timer running in the background. Every time it restarts it picks a random period between 0.75 and 1 seconds.

When a follower receives a heartbeat from the leader, it resets its timer. Thus as long as the leader heartbeats all followers frequently enough (say every 0.5 seconds) they will remain followers.

On missing a heartbeat a follower turns into a candidate. It sends a "are you with me?" to all the other nodes. If a majority response yes, it assumes leadership. If a time limit is reached, it goes back to being a follower, so it will (in the absence of a leader) try another candidacy soon.

On assuming leadership, the current term is incremented. The leader sends heartbeats (containing the term number) to all the other nodes. It does't care whether they respond or not.

The rules ensure that only one node is ever the leader with a given term number.

If a leader receives a heartbeat, it must contain a different term number. If it is higher, the leader switches to being a follower. If it is lower, the leader remains leader (and will therefore send a heartbeat to the other would-be leader). If it's the same, the protocol has broken down somewhere and so the safest thing to do is for it to switch to being a follower.


  while (!cancelled)
  {
    await Task.Delay(random, cancelled);
  
    if   
  }

A candidate launches N parallel calls, and as soon as it has positive responses from > N/2 it transitions.

It waits for 






