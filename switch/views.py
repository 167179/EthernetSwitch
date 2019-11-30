import re
from django.http import HttpResponseRedirect
from django.shortcuts import render
from django.urls import reverse

from Services.InterfaceServices import IInterfaceServices
from switch.models import Port, Interfaces, InterfacesConfig, get_ports_from_config, save_ports
from Services.ServiceFactory import factory


def index(request):
    interface_service: IInterfaceServices = factory.create('IInterfaceServices')
    interfaces = interface_service.get_all_interfaces()
    ports = [Port(interface) for interface in interfaces]

    context = {
        'content': 'Hello',
        'title': 'Ethernet switch',
        'ports': ports
    }

    return render(request, 'switch/index.html', context)


def submit(request):
    ports = []
    print(request.POST)
    print(request.POST.getlist('names'))
    for portName in request.POST.getlist('names'):
        value = int(request.POST['port-' + portName + '-value'])
        enabled = portName in request.POST.getlist('enabled-ports')
        tagged = portName in request.POST.getlist('tagged-ports')
        ports.append(Port(portName, value, enabled, tagged))

    print(ports)
    config = Interfaces \
        .init('mybridge', ports) \
        .add_modify_stamp() \
        .add_default_interface() \
        .up_interfaces() \
        .add_loopback() \
        .allow_vagrant() \
        .add_no_tag_vlan() \
        .create_config()

    with open(InterfacesConfig.interface_config_path, 'w+') as f:
        f.write(config)

    return HttpResponseRedirect(reverse('index'))
