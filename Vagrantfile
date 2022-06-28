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
	    env.vm.network "private_network", ip: "192.168.0.88"

		env.vm.synced_folder ".", "/home/vagrant/env/"

		env.vm.provision "shell", inline: <<-SHELL
			sudo apt-get remove docker docker-engine docker.io containerd runc
			sudo apt-get update
			sudo apt-get install \
			    ca-certificates \
			    curl \
			    gnupg \
			    lsb-release -y
			curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
			echo \
			"deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
			$(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
			sudo apt-get update
			sudo apt-get install docker-ce docker-ce-cli containerd.io docker-compose-plugin -y
			sudo apt install docker-compose -y

			sudo docker-compose up --build

	      	echo "Environment ready!"
	    SHELL
	end
end