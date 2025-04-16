# Use the official SQL Server 2019 image as the base image.
FROM mcr.microsoft.com/mssql/server:2019-latest

# Switch to root to install additional packages.
USER root

# Update package lists and install prerequisites.
RUN apt-get update && \
    apt-get install -y curl apt-transport-https gnupg

# Add Microsoft's package repository key and repository file.
RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
    curl https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-2019.list | tee /etc/apt/sources.list.d/mssql-server.list

# Update package lists and install the Full-Text Search component.
RUN apt-get update && \
    apt-get install -y mssql-server-fts && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Switch back to the mssql user.
USER mssql

# Expose the default SQL Server port.
EXPOSE 1433

# Start SQL Server.
CMD ["/opt/mssql/bin/sqlservr"]
