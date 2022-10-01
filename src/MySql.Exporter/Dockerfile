FROM mysql:8
ENV MYSQL_ROOT_PASSWORD=Password12!
ENV MYSQL_DATABASE=akka_persistence_tests

# COPY output/backup.sql /docker-entrypoint-initdb.d/
ADD output/mysql.tar /var/lib
