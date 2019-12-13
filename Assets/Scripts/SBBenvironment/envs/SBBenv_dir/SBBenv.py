import gym
class SBBenv(gym.Env):
    def __init__(self):
        print('Environment initialized')
    def step(self):
        print('Step successful!')
    def reset(self):
        print('Environment reset')