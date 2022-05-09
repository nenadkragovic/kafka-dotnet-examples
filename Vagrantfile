# -*- mode: ruby -*-
# vi: set ft=ruby :

# Vagrantfile API/syntax version. Don't touch unless you know what you're doing!
VAGRANTFILE_API_VERSION = "2"

Vagrant.configure(VAGRANTFILE_API_VERSION) do |config|

  	config.vm.define "kafka" do |env|
		env.vm.provider "virtualbox" do |v|
			  v.memory = 8192
			  v.cpus = 4
		end
	    env.vm.box = "ubuntu/bionic64"
	    env.vm.network "private_network", ip: "192.168.77.1"

		env.vm.synced_folder ".", "/home/vagrant/env/"

		env.vm.provision "shell", inline: <<-SHELL
			echo "Installing docker"
			sudo apt update
			sudo apt-get -y install curl apt-transport-https ca-certificates software-properties-common
			sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
			sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
			sudo apt update
			sudo apt -y install docker-ce
			sudo apt -y install docker-compose

			echo "Composing the environment:"
			sudo mkdir -p /etc/systemd/system/docker.service.d
			sudo echo '[Service]Environment="HTTP_PROXY=http://8.8.8.8:80/"' >> /etc/systemd/system/docker.service.d/http-proxy.conf

			sudo docker-compose up --build

	      	echo "Environment ready!"
	    SHELL
	end
end