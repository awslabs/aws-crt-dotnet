
# Copyright 2010-2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License").
# You may not use this file except in compliance with the License.
# A copy of the License is located at
#
#  http://aws.amazon.com/apache2.0
#
# or in the "license" file accompanying this file. This file is distributed
# on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
# express or implied. See the License for the specific language governing
# permissions and limitations under the License.

from fetch import fetch_script
from project import Import

import os

URLs = {
    'linux': 'https://dot.net/v1/dotnet-install.sh',
    'windows': 'https://dot.net/v1/dotnet-install.ps1',
    'macos': 'https://dot.net/v1/dotnet-install.sh',
}


class DotNet(Import):
    def __init__(self, **kwargs):
        super().__init__(config={}, **kwargs)
        self.path = None
        self.installed = False
        self.channel = 'LTS'

    def resolved(self):
        return True

    def install(self, env):
        if self.installed:
            return

        sh = env.shell
        script_url = URLs.get(env.spec.target, None)
        if not script_url:
            raise EnvironmentError(
                'Target OS {} does not have dotnet support'.format(env.spec.target))

        script = os.path.basename(script_url)
        fetch_script(script_url, script)

        arch = env.spec.arch
        if env.spec.target == 'windows':
            command = '{} -Channel {} -Architecture {}'.format(
                script, channel, arch)
        else:
            command = '{} --channel {} --architecture {}'.format(
                script, channel, arch)

        sh.exec(command, check=True)
        self.installed = True


class DotNetCore(DotNet):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.channel = 'LTS'


class DotNetCore21(DotNetCore):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.channel = '2.1'


class DotNetCore31(DotNetCore):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.channel = '3.1'
